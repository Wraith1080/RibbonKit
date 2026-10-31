using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerAuthoringTests
{
    [Fact]
    public void Draft_roundtrip_is_detached_and_noop_does_not_create_an_undo_entry() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var initial = RibbonDesignCommitter.Read(ribbon, host);
        var draft = RibbonDesignSnapshot.Deserialize(initial.Serialize());
        draft.Tabs[0].Properties.Single(property => property.Name == "Text").Value = "Draft caption";
        Assert.Equal("Home", ribbon.Tabs[0].Text);
        Assert.Equal(initial.Revision, RibbonDesignCommitter.Read(ribbon, host).Revision);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, initial)); Assert.Equal(0, host.Transactions);
        Assert.Equal(0, host.Changes.Changed);
    });

    [Fact]
    public void Apply_keeps_component_identity_resources_events_bindings_and_selected_artwork() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var button = (RibbonButtonItem)group.Items[0];
        var field = (RibbonTextBoxItem)group.Items[1]; var gallery = (RibbonGalleryItem)group.Items[2]; var selected = gallery.SelectedItem;
        using var image = new Bitmap(16, 16); button.Image = image;
        int clicks = 0; button.Click += (_, _) => clicks++;
        using var source = new BindingSource { DataSource = new State() };
        field.DataBindings.Add(new Binding(nameof(field.Value), source, nameof(State.Value), false, DataSourceUpdateMode.OnPropertyChanged));
        var binding = field.DataBindings[0]; var reference = new RibbonQuickAccessReference { Source = button }; ribbon.QuickAccessItems.Add(reference);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        var children = draft.Tabs[0].Children[0].Children; children.Reverse();
        children.Single(node => node.Kind == "Button").Properties.Single(property => property.Name == "Text").Value = "Authored command";
        var newTab = New(draft, "Tab", "extraTab"); draft.Tabs.Add(newTab);
        var newGroup = New(draft, "Group", "extraGroup"); newTab.Children.Add(newGroup);
        newGroup.Children.Add(New(draft, "CheckBox", "extraCheck"));
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft));
        Assert.Equal(1, host.Transactions); Assert.Equal(1, host.Committed); Assert.Equal(0, host.Cancelled);
        Assert.Equal("Edit Ribbon", host.TransactionDescription);
        Assert.Same(button, group.Items[2]); Assert.Same(field, group.Items[1]); Assert.Same(image, button.Image);
        Assert.Same(binding, field.DataBindings[0]); Assert.Same(button, reference.Source);
        Assert.Same(selected, gallery.SelectedItem); Assert.Equal("Authored command", button.Text);
        button.PerformClick(); Assert.Equal(1, clicks);
        field.Value = "Bound value"; Assert.Equal("Bound value", ((State)source.DataSource!).Value);
        Assert.Single(ribbon.Tabs[1].Groups[0].Items); Assert.IsType<RibbonCheckBoxItem>(ribbon.Tabs[1].Groups[0].Items[0]);
        Assert.Equal("extraTab", ribbon.Tabs[1].Site!.Name);
    });

    [Fact]
    public void Preexisting_unsited_components_are_named_without_replacing_them_or_losing_gallery_selection() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var button = group.Items[0]; var gallery = (RibbonGalleryItem)group.Items[2]; var selected = gallery.SelectedItem;
        foreach (var component in host.Container.Components.Cast<IComponent>().Where(component => !ReferenceEquals(component, ribbon)).ToArray()) host.Container.Remove(component);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        draft.Nodes.Single(node => node.Kind == "Button").Properties.Single(property => property.Name == "Text").Value = "Updated";
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Same(button, group.Items[0]); Assert.Equal("Updated", button.Text); Assert.NotNull(button.Site);
        Assert.Same(selected, gallery.SelectedItem); Assert.NotNull(selected!.Site);
    });

    [Fact]
    public void Changed_native_choices_retain_the_authored_selection_and_free_text() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var choice = new RibbonComboBoxItem { Id = "choice" }; choice.Items.AddRange("Alpha", "Beta"); choice.SelectedIndex = 1;
        group.Items.Add(choice); host.Container.Add(choice, "choice");
        var draft = RibbonDesignCommitter.Read(ribbon, host); var node = draft.Nodes.Single(node => node.Kind == "ComboBox"); node.Choices.Add("Gamma");
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Equal(1, choice.SelectedIndex); Assert.Equal("Beta", choice.Value);
        choice.IsEditable = true; choice.Value = "Exact free text";
        draft = RibbonDesignCommitter.Read(ribbon, host); node = draft.Nodes.Single(node => node.Kind == "ComboBox"); node.Choices.Add("Delta");
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Equal(-1, choice.SelectedIndex); Assert.Equal("Exact free text", choice.Value);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Editable_mode_and_free_text_apply_together_without_stale_index_overriding_the_value(bool existing) => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; RibbonComboBoxItem? choice = null;
        if (existing)
        {
            choice = new RibbonComboBoxItem { Id = "choice" }; choice.Items.AddRange("Alpha", "Beta"); choice.SelectedIndex = 0;
            group.Items.Add(choice); host.Container.Add(choice, "choice");
        }
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        var node = existing ? draft.Nodes.Single(node => node.Kind == "ComboBox") : New(draft, "ComboBox", "choice");
        if (!existing) draft.Tabs[0].Children[0].Children.Add(node);
        node.Properties.Single(property => property.Name == "IsEditable").Value = "True";
        node.Properties.Single(property => property.Name == "Value").Value = "Exact free text";
        node.Choices.Add("Gamma");
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        choice = group.Items.OfType<RibbonComboBoxItem>().Single();
        Assert.True(choice.IsEditable); Assert.Equal("Exact free text", choice.Value); Assert.Equal(-1, choice.SelectedIndex);
    });

    [Fact]
    public void Checkbox_draft_uses_one_three_state_property_and_applies_a_new_mixed_check() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host); var draft = RibbonDesignCommitter.Read(ribbon, host);
        var node = New(draft, "CheckBox", "mixedCheck");
        Assert.DoesNotContain(node.Properties, property => property.Name == "Checked");
        node.Properties.Single(property => property.Name == "CheckState").Value = "Indeterminate";
        draft.Tabs[0].Children[0].Children.Add(node); RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Equal(CheckState.Indeterminate, ribbon.Tabs[0].Groups[0].Items.OfType<RibbonCheckBoxItem>().Single().CheckState);
    });

    [Fact]
    public void Choice_insertion_preserves_the_selected_value_and_append_preserves_a_duplicate_index() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var choice = new RibbonComboBoxItem { Id = "choice" }; choice.Items.AddRange("Alpha", "Beta", "Beta"); choice.SelectedIndex = 2;
        ribbon.Tabs[0].Groups[0].Items.Add(choice); host.Container.Add(choice, "choice");
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.Nodes.Single(node => node.Kind == "ComboBox").Choices.Add("Gamma");
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Equal(2, choice.SelectedIndex);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.Nodes.Single(node => node.Kind == "ComboBox").Choices.Insert(0, "New");
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Equal("Beta", choice.Value); Assert.True(choice.SelectedIndex >= 2);
    });

    [Theory]
    [InlineData("width")] [InlineData("identity")] [InlineData("kind")] [InlineData("stale")] [InlineData("qat")] [InlineData("schema")]
    public void Invalid_or_stale_drafts_are_rejected_before_any_live_designer_transaction(string failure) => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var button = (RibbonButtonItem)group.Items[0]; var field = (RibbonTextBoxItem)group.Items[1];
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        if (failure == "width") draft.Nodes.Single(node => node.Kind == "TextBox").Properties.Single(property => property.Name == "InputWidth").Value = "0";
        if (failure == "identity") draft.Nodes.Single(node => node.Kind == "TextBox").Properties.Single(property => property.Name == "Id").Value = button.Id;
        if (failure == "kind") draft.Tabs[0].Kind = "Button";
        if (failure == "schema") draft.Nodes.Single(node => node.Kind == "TextBox").Properties.RemoveAll(property => property.Name == "Value");
        if (failure == "stale") button.Text = "External edit";
        if (failure == "qat") { ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = button }); draft.Tabs[0].Children[0].Children.RemoveAt(0); }
        var before = RibbonDesignCommitter.Read(ribbon, host).Revision;
        Assert.ThrowsAny<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft));
        Assert.Equal(0, host.Transactions); Assert.Equal(before, RibbonDesignCommitter.Read(ribbon, host).Revision);
        Assert.Same(button, group.Items[0]); Assert.Same(field, group.Items[1]); Assert.False(button.IsDisposed);
    });

    [Fact]
    public void A_new_selection_only_choice_can_be_authored_by_value() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host); var draft = RibbonDesignCommitter.Read(ribbon, host);
        var node = New(draft, "ComboBox", "choice"); node.Choices.AddRange(["Alpha", "Beta"]);
        node.Properties.Single(property => property.Name == "Value").Value = "Beta";
        draft.Tabs[0].Children[0].Children.Add(node); RibbonDesignCommitter.Apply(ribbon, host, draft);
        var choice = ribbon.Tabs[0].Groups[0].Items.OfType<RibbonComboBoxItem>().Single();
        Assert.Equal("Beta", choice.Value); Assert.Equal(1, choice.SelectedIndex);
    });

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Removing_a_choice_resolves_the_stale_index_before_live_apply(int removedIndex) => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var choice = new RibbonComboBoxItem { Id = "choice" }; choice.Items.AddRange("Alpha", "Beta"); choice.SelectedIndex = 1;
        ribbon.Tabs[0].Groups[0].Items.Add(choice); host.Container.Add(choice, "choice");
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.Nodes.Single(node => node.Kind == "ComboBox").Choices.RemoveAt(removedIndex);
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Equal(removedIndex == 0 ? "Beta" : "", choice.Value); Assert.Equal(removedIndex == 0 ? 0 : -1, choice.SelectedIndex);
    });

    [Fact]
    public void Removing_an_owned_subtree_uses_the_designer_host_and_preserves_siblings() => FoundationTests.Sta(() =>
    {
        using var host = new Host(); using var ribbon = MakeRibbon(host);
        var group = ribbon.Tabs[0].Groups[0]; var gallery = (RibbonGalleryItem)group.Items[2]; var tile = gallery.SelectedItem!; var button = group.Items[0];
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.Tabs[0].Children[0].Children.RemoveAt(2);
        RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Equal(new IComponent[] { tile, gallery }, host.Destroyed);
        Assert.True(tile.IsDisposed); Assert.True(gallery.IsDisposed); Assert.False(button.IsDisposed); Assert.Same(button, group.Items[0]);
        Assert.Equal(1, host.Committed);
    });

    internal static RibbonControl MakeRibbon(Host host)
    {
        var ribbon = new RibbonControl(); host.Container.Add(ribbon, "ribbon");
        var tab = new RibbonTab { Id = "home", Text = "Home" }; host.Container.Add(tab, "homeTab");
        var group = new RibbonGroup { Id = "commands", Text = "Commands" }; host.Container.Add(group, "commandsGroup");
        var button = new RibbonButtonItem { Id = "apply", Text = "Apply" }; host.Container.Add(button, "applyButton");
        var field = new RibbonTextBoxItem { Id = "name", Text = "Name" }; host.Container.Add(field, "nameField");
        var gallery = new RibbonGalleryItem { Id = "gallery", Text = "Styles" }; host.Container.Add(gallery, "styles");
        var tile = new RibbonGalleryTile { Id = "tile", Text = "Normal" }; host.Container.Add(tile, "normalTile");
        gallery.Items.Add(tile); gallery.SelectedItem = tile; group.Items.AddRange(button, field, gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        return ribbon;
    }
    private static RibbonDesignNode New(RibbonDesignSnapshot draft, string kind, string name)
    {
        var template = draft.Templates.Single(node => node.Kind == kind);
        var copy = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = [template] }.Serialize()).Tabs[0];
        copy.Key = "new:" + Guid.NewGuid(); copy.ComponentName = name; copy.Properties.Single(property => property.Name == "Id").Value = name;
        return copy;
    }
    private sealed class State { public string Value { get; set; } = "Initial"; }
    internal sealed class Host : IDesignerHost, IDisposable
    {
        public IContainer Container { get; } = new Container();
        internal Changes Changes { get; } = new();
        internal int Transactions, Committed, Cancelled;
        internal string TransactionDescription = "";
        internal List<IComponent> Destroyed { get; } = [];
        internal ISelectionService? Selection;
        public object? GetService(Type serviceType) => serviceType == typeof(IComponentChangeService) ? Changes : serviceType == typeof(IDesignerHost) ? this : serviceType == typeof(ISelectionService) ? Selection : null;
        public IComponent CreateComponent(Type type) => CreateComponent(type, type.Name + Container.Components.Count);
        public IComponent CreateComponent(Type type, string? name) { var component = (IComponent)Activator.CreateInstance(type)!; Container.Add(component, name); return component; }
        public void DestroyComponent(IComponent component) { Destroyed.Add(component); component.Dispose(); }
        public DesignerTransaction CreateTransaction() => CreateTransaction("");
        public DesignerTransaction CreateTransaction(string? description) { Transactions++; TransactionDescription = description ?? ""; return new Transaction(this, description); }
        private sealed class Transaction(Host host, string? description) : DesignerTransaction(description ?? "")
        { protected override void OnCancel() => host.Cancelled++; protected override void OnCommit() => host.Committed++; }
        public bool Loading => false; public bool InTransaction => false; public IComponent RootComponent => Container.Components["ribbon"]!; public string RootComponentClassName => RootComponent.GetType().FullName!;
        string IDesignerHost.TransactionDescription => TransactionDescription;
        public void Activate() { } public IDesigner? GetDesigner(IComponent component) => null; public Type? GetType(string typeName) => Type.GetType(typeName);
        public void AddService(Type type, ServiceCreatorCallback callback) { } public void AddService(Type type, ServiceCreatorCallback callback, bool promote) { }
        public void AddService(Type type, object instance) { } public void AddService(Type type, object instance, bool promote) { }
        public void RemoveService(Type type) { } public void RemoveService(Type type, bool promote) { }
        public event EventHandler? Activated { add { } remove { } } public event EventHandler? Deactivated { add { } remove { } } public event EventHandler? LoadComplete { add { } remove { } }
        public event DesignerTransactionCloseEventHandler? TransactionClosed { add { } remove { } } public event DesignerTransactionCloseEventHandler? TransactionClosing { add { } remove { } }
        public event EventHandler? TransactionOpened { add { } remove { } } public event EventHandler? TransactionOpening { add { } remove { } }
        public void Dispose() => Container.Dispose();
    }
    internal sealed class Changes : IComponentChangeService
    {
        internal int Changed;
        public void OnComponentChanging(object component, MemberDescriptor? member) { }
        public void OnComponentChanged(object component, MemberDescriptor? member, object? oldValue, object? newValue) => Changed++;
        public event ComponentEventHandler? ComponentAdded { add { } remove { } } public event ComponentEventHandler? ComponentAdding { add { } remove { } }
        public event ComponentChangedEventHandler? ComponentChanged { add { } remove { } } public event ComponentChangingEventHandler? ComponentChanging { add { } remove { } }
        public event ComponentEventHandler? ComponentRemoved { add { } remove { } } public event ComponentEventHandler? ComponentRemoving { add { } remove { } }
        public event ComponentRenameEventHandler? ComponentRename { add { } remove { } }
    }
}
