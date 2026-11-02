using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerMenuPresentationTests
{
    [Theory]
    [InlineData(false, "Standard")] [InlineData(true, "Standard")]
    [InlineData(false, "IllustratedChoices")] [InlineData(true, "IllustratedChoices")]
    public void Header_presentation_and_child_descriptions_are_editable_detached_and_render_after_ok(bool split, string presentation) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var menu = split ? new RibbonSplitButtonItem { Text = "Paste" } : new RibbonDropDownItem { Text = "Paste" };
        host.Container.Add(menu, "pasteMenu"); ribbon.Tabs[0].Groups[0].Items.Add(menu);
        var text = new RibbonButtonItem { Text = "Paste text only", Icon = RibbonStockIcons.Create("save") };
        var separator = new RibbonMenuSeparatorItem(); var special = new RibbonButtonItem { Text = "Paste special", Icon = text.Icon };
        menu.Items.AddRange(text, separator, special);
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        var node = draft.Nodes.Single(n => n.ComponentName == "pasteMenu");
        var properties = TypeDescriptor.GetProperties(new RibbonEditorDialog.DraftProperties(node));
        Assert.Equal(new[] { "Standard", "IllustratedChoices" }, properties["MenuPresentation"]!.Converter.GetStandardValues()!.Cast<string>());
        Assert.DoesNotContain(draft.Nodes.Single(n => n.Kind == "Gallery").Properties, p => p.Name is "DropDownHeader" or "MenuPresentation");
        properties["DropDownHeader"]!.SetValue(null, "Paste options"); properties["MenuPresentation"]!.SetValue(null, presentation);
        Set(node.Children[0], "MenuDescription", "Insert clipboard text without its original formatting.");
        Set(node.Children[2], "MenuDescription", "Choose which clipboard format to insert into the document.");
        Assert.Contains("hover ScreenTip", node.Children[0].Properties.Single(p => p.Name == "MenuDescription").Description);
        using (var cancelled = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Cancel must not commit"))) cancelled.DialogResult = DialogResult.Cancel;
        RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Equal("", menu.DropDownHeader); Assert.Equal("", text.MenuDescription); Assert.Equal(RibbonMenuPresentation.Standard, menu.MenuPresentation); Assert.Equal(0, host.Transactions);
        using (var accepted = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; })) accepted.AcceptDraft();
        Assert.Equal(1, host.Committed); Assert.Equal("Paste options", menu.DropDownHeader); Assert.Equal(Enum.Parse<RibbonMenuPresentation>(presentation), menu.MenuPresentation);
        Assert.Same(text, menu.Items[0]); Assert.Same(separator, menu.Items[1]); Assert.Same(special, menu.Items[2]);
        Assert.Equal("", text.ScreenTipDescription);
        var reopened = RibbonDesignCommitter.Read(ribbon, host).Nodes.Single(n => n.ComponentName == "pasteMenu");
        Assert.Equal(menu.DropDownHeader, reopened.Properties.Single(p => p.Name == "DropDownHeader").Value);
        Assert.Equal(text.MenuDescription, reopened.Children[0].Properties.Single(p => p.Name == "MenuDescription").Value);
        using var painter = new Recorder(); ribbon.Renderer = painter;
        using var popup = new RibbonPopupSurface(ribbon, menu, new Size(1200, 900));
        Assert.Equal(3, popup.Targets.Count); Assert.False(popup.Targets[1].Enabled);
        Assert.True(popup.Targets[0].Bounds.Top > ribbon.Font.Height);
        using var pixels = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(pixels, popup.ClientRectangle);
        Assert.Contains(menu.DropDownHeader, painter.Text); Assert.Contains(text.Text, painter.Text); Assert.Contains(text.MenuDescription, painter.Text); Assert.Contains(special.MenuDescription, painter.Text);
        if (!split && Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } capture)
        { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"designer-menu-{presentation}-{ribbon.DeviceDpi}dpi.png")); }
    });

    [Fact]
    public void Menu_settings_undo_and_redo_in_one_framework_unit_and_invalid_drafts_do_not_apply() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "tab"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "group"); tab.Groups.Add(group);
        var menu = (RibbonSplitButtonItem)host.CreateComponent(typeof(RibbonSplitButtonItem), "paste"); group.Items.Add(menu);
        var child = (RibbonButtonItem)host.CreateComponent(typeof(RibbonButtonItem), "textOnly"); menu.Items.Add(child);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new UndoRecorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); var node = draft.Nodes.Single(n => n.Kind == "SplitButton");
        Set(node, "DropDownHeader", "Paste options"); Set(node, "MenuPresentation", "Invalid"); Set(node.Children[0], "MenuDescription", "Keep text only.");
        Assert.Throws<FormatException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(0, undo.Count); Assert.Equal("", menu.DropDownHeader);
        Set(node, "MenuPresentation", "IllustratedChoices"); Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal("", menu.DropDownHeader); Assert.Equal(RibbonMenuPresentation.Standard, menu.MenuPresentation); Assert.Equal("", child.MenuDescription);
        undo.Toggle(); Assert.Equal("Paste options", menu.DropDownHeader); Assert.Equal(RibbonMenuPresentation.IllustratedChoices, menu.MenuPresentation); Assert.Equal("Keep text only.", child.MenuDescription);
    });

    [Fact]
    public void Header_description_and_presentation_use_standard_property_serialization_and_defaults() => FoundationTests.Sta(() =>
    {
        using var menu = new RibbonDropDownItem { DropDownHeader = "Paste options", MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        using var child = new RibbonButtonItem { MenuDescription = "Keep text only." };
        foreach (var (component, name, propertyName, value) in new (IComponent, string, string, string)[]
        { (menu, "paste", "DropDownHeader", "Paste options"), (menu, "paste", "MenuPresentation", "IllustratedChoices"), (child, "textOnly", "MenuDescription", "Keep text only.") })
        {
            var property = TypeDescriptor.GetProperties(component)[propertyName]!; var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(component, name);
            var serializer = (CodeDomSerializer)manager.GetSerializer(component.GetType(), typeof(CodeDomSerializer))!;
            var assignment = Assert.Single(serializer.SerializeMember(manager, component, property).OfType<CodeAssignStatement>());
            if (propertyName == "MenuPresentation") Assert.Equal(value, Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName);
            else Assert.Equal(value, Assert.IsType<CodePrimitiveExpression>(assignment.Right).Value);
            Assert.Empty(serialization.Errors.Cast<object>()); property.ResetValue(component); Assert.False(property.ShouldSerializeValue(component));
        }
    });
    private static void Set(RibbonDesignNode node, string name, string value) => node.Properties.Single(p => p.Name == name).Value = value;
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal readonly List<string> Text = [];
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Text.Add(text); base.DrawText(context, text, color, flags); }
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
