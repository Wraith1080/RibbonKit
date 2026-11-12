using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.CodeDom;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ApplicationMenuDesignerTests
{
    [Fact]
    public void Framework_undo_redo_restores_nested_pane_and_footer_collections_and_default_header() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon"); ((Form)host.RootComponent).Controls.Add(ribbon);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        var pane = (RibbonDropDownItem)host.CreateComponent(typeof(RibbonDropDownItem), "openPane"); pane.Text = "Open"; ribbon.FileMenu.Items.Add(pane);
        var command = (RibbonButtonItem)host.CreateComponent(typeof(RibbonButtonItem), "optionsCommand"); command.Text = "Options"; ribbon.ApplicationMenu.FooterItems.Add(command);
        var draft = RibbonDesignCommitter.ReadApplicationMenu(ribbon, host);
        var service = new CodeDomComponentSerializationService(host); using var store = service.CreateStore();
        service.SerializeMemberAbsolute(store, ribbon.ApplicationMenu.DefaultPane, TypeDescriptor.GetProperties(ribbon.ApplicationMenu.DefaultPane)["DropDownHeader"]!); store.Close(); Assert.Empty(store.Errors.Cast<object>());
        ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Probe";
        service.DeserializeTo(store, host.Container); Assert.Empty(store.Errors.Cast<object>()); Assert.Equal("", ribbon.ApplicationMenu.DefaultPane.DropDownHeader);
        using var undo = new UndoRecorder(host);
        draft.Entries[1].Properties.Single(p => p.Name == "DropDownHeader").Value = "Recent documents";
        draft.Entries[0].Children[0].Properties.Single(p => p.Name == "Text").Value = "Open documents";
        var extra = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = { draft.Templates.Single(t => t.Kind == "Button") } }.Serialize()).Tabs[0];
        extra.Key = "new:extra"; extra.ComponentName = "extraCommand"; extra.Properties.Single(p => p.Name == "Id").Value = "application:extra";
        draft.Entries[1].Children.Add(extra);
        Assert.True(RibbonDesignCommitter.ApplyApplicationMenu(ribbon, host, draft)); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal("", ribbon.ApplicationMenu.DefaultPane.DropDownHeader); Assert.Equal("Open", ribbon.FileMenu.Items[0].Text); Assert.Single(ribbon.ApplicationMenu.FooterItems); Assert.Empty(ribbon.ApplicationMenu.DefaultPane.Items);
        undo.Toggle(); Assert.Equal("Recent documents", ribbon.ApplicationMenu.DefaultPane.DropDownHeader); Assert.Equal("Open documents", ribbon.FileMenu.Items[0].Text); Assert.Single(ribbon.ApplicationMenu.FooterItems); Assert.Single(ribbon.ApplicationMenu.DefaultPane.Items);
        var general = RibbonDesignCommitter.Read(ribbon, host);
        Assert.Contains(ribbon.ApplicationMenu.DefaultPane.Items[0].Id, general.ReservedIds);
        Assert.Contains(command.Id, general.ReservedIds);
        var generatedManager = new DesignerSerializationManager(host); using var generatedSession = generatedManager.CreateSession();
        var manager = (IDesignerSerializationManager)generatedManager;
        foreach (IComponent component in host.Container.Components) if (component.Site?.Name is { } name) manager.SetName(component, name);
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationMenu)]!);
        using var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
        foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new());
        Assert.Contains("ApplicationMenu.DefaultPane", code.ToString()); Assert.DoesNotContain(".ApplicationMenuDefaultPane.", code.ToString());
        Assert.Empty(generatedManager.Errors.Cast<object>());
    });

    [Fact]
    public void Code_dom_serializes_stable_pane_footer_and_content_type_and_reset_omits_default() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent documents";
        ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Id = "recent:report", Text = "Report" });
        ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Id = "footer:options", Text = "Options" });
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession(); var manager = (IDesignerSerializationManager)serialization;
        manager.SetName(ribbon, "ribbon"); manager.SetName(ribbon.ApplicationMenu.DefaultPane.Items[0], "recentCommand"); manager.SetName(ribbon.ApplicationMenu.FooterItems[0], "optionsCommand"); var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationMenu)]!);
        Assert.Empty(serialization.Errors.Cast<object>());
        using var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp"); using var generated = new StringWriter();
        foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, generated, new());
        string code = generated.ToString(); Assert.Contains("ApplicationMenu.DefaultPane", code); Assert.True(code.Contains("ApplicationMenu.FooterItems.AddRange"), code); Assert.Contains("Recent documents", code);
        var property = TypeDescriptor.GetProperties(ribbon.ApplicationMenu.DefaultPane)["ContentType"]!;
        ribbon.ApplicationMenu.DefaultPane.ContentType = typeof(ApplicationMenuTestControl); Assert.True(property.ShouldSerializeValue(ribbon.ApplicationMenu.DefaultPane));
        property.ResetValue(ribbon.ApplicationMenu.DefaultPane); Assert.Null(ribbon.ApplicationMenu.DefaultPane.ContentType); Assert.False(property.ShouldSerializeValue(ribbon.ApplicationMenu.DefaultPane));
    });
    [Fact]
    public void Cancel_and_selection_only_noop_keep_live_components_and_create_no_transaction() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); draft.SelectionKey = draft.Entries[0].Children[0].Key;
        Assert.False(RibbonDesignCommitter.ApplyApplicationMenu(f.Ribbon, f.Host, draft)); Assert.Equal(0, f.Host.Transactions);
        draft.Entries[0].Children[0].Properties.Single(p => p.Name == "Text").Value = "Draft only";
        using var editor = new RibbonApplicationMenuDialog(draft, _ => throw new Exception("Cancel must not commit"), d => RibbonDesignCommitter.PreviewApplicationMenu(f.Ribbon, f.Host, d));
        Assert.Equal("Open", f.Pane.Text); Assert.Equal(0, f.Host.Changes.Changed);
    });

    [Fact]
    public void Apply_reorder_and_properties_use_one_transaction_preserve_content_bindings_events_and_component_identity() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var handle = f.Body.Handle; var draft = f.Read();
        var node = draft.Entries[0].Children[0]; node.Properties.Single(p => p.Name == "Text").Value = "Open documents";
        draft.Entries[0].Children.Reverse(); draft.Entries[1].Properties.Single(p => p.Name == "DropDownHeader").Value = "Recent documents";
        draft.Settings.Single(p => p.Name == "FileVisible").Value = "True";
        int clicks = 0; f.Footer.Click += (_, _) => clicks++;
        Assert.True(RibbonDesignCommitter.ApplyApplicationMenu(f.Ribbon, f.Host, draft));
        Assert.Equal(1, f.Host.Committed); Assert.Equal("Edit Application Menu", f.Host.TransactionDescription);
        Assert.Same(f.Pane, f.Ribbon.FileMenu.Items[1]); Assert.Same(f.Body, f.Pane.Content); Assert.Equal(handle, f.Body.Handle); Assert.Null(f.Body.Parent);
        Assert.Equal("Open documents", f.Pane.Text); Assert.Equal("Recent documents", f.Ribbon.ApplicationMenu.DefaultPane.DropDownHeader); Assert.True(f.Ribbon.FileMenu.Visible);
        Assert.Same(f.Footer, Assert.Single(f.Ribbon.ApplicationMenu.FooterItems)); Assert.Equal(0, clicks); f.Footer.PerformClick(); Assert.Equal(1, clicks);
        Assert.False(RibbonDesignCommitter.ApplyApplicationMenu(f.Ribbon, f.Host, f.Read())); Assert.Equal(1, f.Host.Transactions);
    });

    [Theory]
    [InlineData("schema")] [InlineData("stale")] [InlineData("type")] [InlineData("duplicate")]
    [InlineData("outside")] [InlineData("svg")] [InlineData("content")]
    public void Invalid_drafts_fail_before_mutation(string failure) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); var node = draft.Entries[0].Children[0];
        switch (failure)
        {
            case "schema": node.Properties.RemoveAt(0); break;
            case "stale": f.Footer.Text = "External edit"; break;
            case "type": node.Kind = "Button"; break;
            case "duplicate": node.ComponentName = draft.Entries[2].Children[0].ComponentName; break;
            case "outside": node.Properties.Single(p => p.Name == "Id").Value = f.Ribbon.Tabs[0].Id; break;
            case "svg": node.Icon = new() { Kind = "Svg", Source = "<svg" }; break;
            case "content": f.Pane.Content = null; break;
        }
        string before = f.Read().Revision;
        Assert.ThrowsAny<Exception>(() => RibbonDesignCommitter.ApplyApplicationMenu(f.Ribbon, f.Host, draft));
        Assert.Equal(before, f.Read().Revision); Assert.Equal(0, f.Host.Transactions); Assert.Equal(0, f.Host.Changes.Changed);
    });

    [Fact]
    public void Preview_selects_draft_panes_and_never_constructs_paints_or_reparents_application_content() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); draft.SelectionKey = draft.Entries[0].Children[0].Key;
        var before = f.Read().Revision; var original = RibbonDesignCommitter.PreviewApplicationMenu(f.Ribbon, f.Host, draft);
        f.Ribbon.Theme = f.Ribbon.Theme with { ApplicationMenuAppearance = new() { SurroundBackground = new(Color.LightYellow), ActivePaneBackground = new(Color.LightBlue), InnerFrameCornerRadius = 12, InnerFrameBorderWidth = 3, HeaderBold = false, HeaderRuleThickness = 3 } };
        var surface = RibbonDesignCommitter.PreviewApplicationMenu(f.Ribbon, f.Host, draft);
        f.Ribbon.Theme = f.Ribbon.Theme with { ApplicationMenuContent = new() { LargeTextInset = 80, SmallIconSize = 24, ArrowColumnWidth = 38, RichTitleBold = false } };
        var frame = RibbonDesignCommitter.PreviewApplicationMenu(f.Ribbon, f.Host, draft); Assert.NotEqual(original.Png, frame.Png);
        Assert.NotEqual(surface.Png, frame.Png);
        Assert.NotEmpty(frame.Png); Assert.Contains(frame.Regions, r => r.Key == draft.SelectionKey); Assert.Null(f.Body.Parent); Assert.Equal(0, f.Body.Paints);
        Assert.Equal(before, f.Read().Revision); Assert.Equal(0, f.Host.Transactions); Assert.False(f.Ribbon.FileMenu.IsDropDownOpen);
    });

    [Fact]
    public void Added_nested_command_and_footer_removal_keep_one_transaction_and_other_sources() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read(); var template = draft.Templates.Single(n => n.Kind == "Button");
        var node = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = { template } }.Serialize()).Tabs[0];
        node.Key = "new:nested"; node.ComponentName = "nestedCommand"; node.Properties.Single(p => p.Name == "Id").Value = "application:nested";
        draft.Entries[0].Children[0].Children.Add(node); draft.Entries[2].Children.Clear();
        Assert.True(RibbonDesignCommitter.ApplyApplicationMenu(f.Ribbon, f.Host, draft));
        Assert.Single(f.Pane.Items); Assert.Same(f.Host.Container.Components["nestedCommand"], f.Pane.Items[0]); Assert.True(f.Footer.IsDisposed); Assert.False(f.Body.IsDisposed); Assert.Equal(1, f.Host.Committed);
    });
    private sealed class NeverPaint : UserControl
    {
        internal int Paints;
        protected override void OnPaint(PaintEventArgs e) { Paints++; throw new InvalidOperationException("Application content cannot be painted in preview."); }
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly RibbonControl Ribbon;
        internal readonly NeverPaint Body = new();
        internal readonly RibbonDropDownItem Pane;
        internal readonly RibbonButtonItem Footer = new() { Id = "app-options", Text = "Options" };
        internal Fixture()
        {
            Ribbon = DesignerAuthoringTests.MakeRibbon(Host);
            Pane = new() { Id = "app-open", Text = "Open", Content = Body };
            Host.Container.Add(Pane, "openPane"); Host.Container.Add(Footer, "optionsCommand");
            Ribbon.FileMenu.Items.AddRange(Pane, new RibbonButtonItem { Id = "app-save", Text = "Save" });
            Ribbon.ApplicationMenu.FooterItems.Add(Footer);
        }
        internal RibbonBackstageSnapshot Read() => RibbonDesignCommitter.ReadApplicationMenu(Ribbon, Host);
        public void Dispose() { Host.Dispose(); Body.Dispose(); }
    }
}

public sealed class ApplicationMenuTestControl : UserControl { }
