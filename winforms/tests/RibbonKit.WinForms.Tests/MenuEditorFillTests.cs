using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuEditorFillTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Fill_width_uses_the_menu_trailing_edge_and_shared_label_column_in_inert_preview(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
        using var f = new Fixture(painter);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int width in new[] { 380, 620 })
        {
            f.Ribbon.Density = density; f.Ribbon.Theme = f.Ribbon.Theme with { MenuMinimumWidth = width };
            f.SetFill(false); Render(); var fixedFields = record.Fields.ToDictionary(p => p.Key, p => p.Value);
            f.SetFill(true); Render();
            int right = record.Fields[f.Name].Right;
            Assert.All(f.Editors, item =>
            {
                var field = record.Fields[item]; Assert.Equal(right, field.Right); Assert.True(field.Width > fixedFields[item].Width);
                Assert.Equal(fixedFields[item].Left, field.Left); Assert.Equal(fixedFields[item].Top, field.Top); Assert.Equal(fixedFields[item].Height, field.Height);
                Assert.Equal(80, ((IRibbonEditorItem)item).InputWidth);
            });
            Assert.Equal(record.Text[f.Action.Text].Left, record.Text[f.Name.Text].Left);
            Assert.Equal(record.Text[f.Action.Text].Left, record.Fields[f.Hidden].Left);
            using var popup = new RibbonPopupSurface(f.Ribbon, f.Menu, new(1800, 16384), f.Menu);
            int trailing = RibbonLayoutEngine.Scale(18, f.Ribbon.DeviceDpi);
            Assert.Equal(popup.Targets[0].Bounds.Right - trailing, right);

            void Render()
            {
                var keys = f.Menu.Items.ToDictionary(i => (RibbonElement)i, i => i.Text); keys[f.Menu] = "menu";
                var preview = RibbonPopupPreviewRenderer.Render(f.Ribbon, f.Menu, "menu", "", keys);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_FILL_CAPTURE") is not { Length: > 0 } capture) return;
                Directory.CreateDirectory(capture); File.WriteAllBytes(Path.Combine(capture, $"preview-{office}-{density}-{width}-{f.Name.FillMenuWidth}.png"), Convert.FromBase64String(preview.Png));
            }
        }
    });

    [Fact]
    public void Fill_keeps_height_and_preferred_minimum_at_synthetic_dpi_and_does_not_stretch_other_surfaces() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            f.Ribbon.Density = density; using var font = new Font("Segoe UI", points * dpi / 96f);
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            int label = f.Editors.Max(i => RibbonEditorLayout.LabelWidth(i, font, dpi));
            foreach (var item in f.Editors)
            {
                f.SetFill(false); int preferred = RibbonEditorLayout.MenuWidth(item, label, font, dpi, density, f.Ribbon.Theme);
                var row = new Rectangle(S(4), 0, preferred + S(200) - S(8), S(80));
                var content = RibbonEditorLayout.ContentBounds(row, item, dpi, density, f.Ribbon.Theme, true);
                var fixedField = RibbonEditorLayout.Field(content, item, label, font, dpi, menu: true);
                var strip = RibbonEditorLayout.Field(row, item, label, font, dpi);
                f.SetFill(true); var filled = RibbonEditorLayout.Field(content, item, label, font, dpi, menu: true);
                Assert.Equal(preferred, RibbonEditorLayout.MenuWidth(item, label, font, dpi, density, f.Ribbon.Theme));
                Assert.Equal(S(200), filled.Width - fixedField.Width); Assert.Equal(fixedField.Location, filled.Location); Assert.Equal(fixedField.Height, filled.Height);
                Assert.Equal(strip, RibbonEditorLayout.Field(row, item, label, font, dpi));
                var narrow = content with { Width = 1 }; Assert.Equal(0, RibbonEditorLayout.Field(narrow, item, label, font, dpi, menu: true).Width);
            }
        }
        using var flyout = new RibbonPopupSurface(f.Ribbon, f.Group, new(500, 500)); Assert.False(flyout.UsesMenuEditorGutter);
        using var constrained = new RibbonPopupSurface(f.Ribbon, f.Menu, new(180, 220)); Assert.True(constrained.Width <= 180); Assert.True(constrained.Height <= 220);
    });

    [Fact]
    public void Native_fill_changes_remeasure_and_reuse_all_editors_without_changing_values_or_height() => FoundationTests.Sta(() =>
    {
        var record = new Record(); using var painter = new Office(record); using var f = new Fixture(painter); using var form = new Form { Width = 1100, Height = 700 };
        form.Controls.Add(f.Ribbon); form.Show(); Application.DoEvents(); nint[]? handles = null;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            f.Ribbon.Density = density; Dictionary<RibbonItem, Rectangle>? fixedFields = null;
            foreach (bool fill in new[] { false, true, false })
            {
                f.SetFill(fill); Application.DoEvents(); f.Menu.ShowDropDown(); var popup = f.Ribbon.Popups.Top;
                // Inspect synchronous layout/native retention before processing
                // external activation messages. This is not an input/focus test.
                Assert.True(popup is not null, $"Native popup remains open: density={density}, fill={fill}");
                Control[] editors = [f.Ribbon.Editors.EditorFor(f.Name)!, f.Ribbon.Editors.EditorFor(f.Combo)!, f.Ribbon.Editors.EditorFor(f.Hidden)!];
                handles ??= editors.Select(c => c.Handle).ToArray(); Assert.Equal(handles, editors.Select(c => c.Handle));
                using var pixels = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(pixels, popup.ClientRectangle);
                if (!fill) fixedFields = record.Fields.ToDictionary(p => p.Key, p => p.Value);
                foreach (var item in f.Editors)
                {
                    Assert.Equal(fixedFields![item].Height, record.Fields[item].Height);
                    if (fill) Assert.True(record.Fields[item].Width > fixedFields[item].Width);
                }
                Assert.Equal("Retained", f.Name.Value); Assert.Equal("Aptos", f.Combo.Value); Assert.Equal("Calibri", f.Hidden.Value);
                if (Environment.GetEnvironmentVariable("RIBBONKIT_MENU_FILL_CAPTURE") is { Length: > 0 } capture)
                { Directory.CreateDirectory(capture); pixels.Save(Path.Combine(capture, $"native-{density}-{fill}-{f.Ribbon.DeviceDpi}dpi.png")); }
                f.Menu.CloseDropDown(); Application.DoEvents();
            }
            f.Menu.CloseDropDown();
        }
    });

    [Fact]
    public void Designer_drafts_apply_once_keep_identity_and_emit_literal_bool_with_default_reset() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var text = ribbon.Tabs[0].Groups[0].Items.OfType<RibbonTextBoxItem>().Single();
        var combo = (RibbonComboBoxItem)host.CreateComponent(typeof(RibbonComboBoxItem), "combo"); ribbon.Tabs[0].Groups[0].Items.Add(combo);
        foreach (var component in new RibbonItem[] { text, combo })
        {
            var property = TypeDescriptor.GetProperties(component)["FillMenuWidth"]!; Assert.Equal("Layout", property.Category); Assert.False(property.ShouldSerializeValue(component));
        }
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        foreach (var node in draft.Nodes.Where(n => n.Kind is "TextBox" or "ComboBox")) node.Properties.Single(p => p.Name == "FillMenuWidth").Value = "True";
        Assert.False(text.FillMenuWidth); Assert.False(combo.FillMenuWidth);
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, host.Committed); Assert.True(text.FillMenuWidth); Assert.True(combo.FillMenuWidth);
        Assert.Same(text, ribbon.Tabs[0].Groups[0].Items.OfType<RibbonTextBoxItem>().Single()); Assert.Same(combo, ribbon.Tabs[0].Groups[0].Items.OfType<RibbonComboBoxItem>().Single());
        foreach (var component in new RibbonItem[] { text, combo })
        {
            var property = TypeDescriptor.GetProperties(component)["FillMenuWidth"]!;
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(component, component is RibbonTextBoxItem ? "text" : "combo");
            var serializer = (CodeDomSerializer)manager.GetSerializer(component.GetType(), typeof(CodeDomSerializer))!;
            var statement = Assert.Single(serializer.SerializeMember(manager, component, property).OfType<CodeAssignStatement>());
            Assert.Equal(true, Assert.IsType<CodePrimitiveExpression>(statement.Right).Value); Assert.Empty(serialization.Errors.Cast<object>());
            property.ResetValue(component); Assert.False(property.ShouldSerializeValue(component)); Assert.False(((IRibbonEditorItem)component).FillMenuWidth);
        }
    });

    [Fact]
    public void Customization_copies_follow_the_source_fill_setting_without_changing_source_width() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
        var name = new RibbonTextBoxItem { Id = "fill.name", FillMenuWidth = true, InputWidth = 80 };
        var combo = new RibbonComboBoxItem { Id = "fill.combo", FillMenuWidth = true, InputWidth = 80 };
        group.Items.AddRange(name, combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.CommandCatalog.CaptureDefaults();
        using var session = ribbon.CommandCatalog.BeginCustomization("fill.test", RibbonCustomizationTarget.Ribbon);
        session.Draft.Ribbon!.Tabs.Add(new RibbonCustomizationTab { Label = "Mine", Groups = [new RibbonCustomizationGroup { Label = "Fields", Commands =
            [new RibbonCustomizationCommand { CommandId = name.Id, Size = RibbonItemSize.Medium }, new RibbonCustomizationCommand { CommandId = combo.Id, Size = RibbonItemSize.Medium }] }] });
        Assert.True(session.Apply()); var copies = ribbon.Tabs.Last().Groups.Single().Items;
        var nameCopy = Assert.IsType<RibbonTextBoxItem>(copies[0]); var comboCopy = Assert.IsType<RibbonComboBoxItem>(copies[1]);
        Assert.True(nameCopy.FillMenuWidth); Assert.True(comboCopy.FillMenuWidth);
        name.FillMenuWidth = false; combo.FillMenuWidth = false;
        Assert.False(nameCopy.FillMenuWidth); Assert.False(comboCopy.FillMenuWidth); Assert.Equal(80, name.InputWidth); Assert.Equal(80, combo.InputWidth);
    });

    [Fact]
    public void Designer_fill_changes_undo_and_redo_through_framework_component_serialization() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "tab"); ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "group"); tab.Groups.Add(group);
        var text = (RibbonTextBoxItem)host.CreateComponent(typeof(RibbonTextBoxItem), "text");
        var combo = (RibbonComboBoxItem)host.CreateComponent(typeof(RibbonComboBoxItem), "combo"); group.Items.AddRange(text, combo);
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new UndoRecorder(host); var draft = RibbonDesignCommitter.Read(ribbon, host);
        foreach (var node in draft.Nodes.Where(n => n.Kind is "TextBox" or "ComboBox")) node.Properties.Single(p => p.Name == "FillMenuWidth").Value = "True";
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Equal(1, undo.Count); Assert.True(text.FillMenuWidth); Assert.True(combo.FillMenuWidth);
        undo.Toggle(); Assert.False(text.FillMenuWidth); Assert.False(combo.FillMenuWidth);
        undo.Toggle(); Assert.True(text.FillMenuWidth); Assert.True(combo.FillMenuWidth);
    });

    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }

    private sealed class Fixture : IDisposable
    {
        internal RibbonControl Ribbon { get; }
        internal RibbonGroup Group { get; } = new();
        internal RibbonDropDownItem Menu { get; } = new();
        internal RibbonButtonItem Action { get; } = new() { Text = "Action" };
        internal RibbonTextBoxItem Name { get; } = new() { Text = "Name", Value = "Retained", InputWidth = 80 };
        internal RibbonComboBoxItem Combo { get; } = new() { Text = "Font family", InputWidth = 80 };
        internal RibbonComboBoxItem Hidden { get; } = new() { Text = "Hidden chooser", InputWidth = 80, ShowLabel = false };
        internal RibbonItem[] Editors => [Name, Combo, Hidden];
        internal Fixture(RibbonRenderer? painter = null)
        {
            Ribbon = new() { Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, Theme = RibbonThemes.Office2024Light with
                { MenuIconSize = 36, MenuLeadingPadding = 14, MenuIconTextGap = 16, MenuTrailingPadding = 18, MenuMinimumWidth = 520 } };
            Combo.Items.Add("Aptos"); Combo.SelectedIndex = 0; Hidden.Items.Add("Calibri"); Hidden.SelectedIndex = 0;
            Menu.Items.AddRange(Action, Name, Combo, Hidden); Group.Items.Add(Menu); var tab = new RibbonTab(); tab.Groups.Add(Group); Ribbon.Tabs.Add(tab);
        }
        internal void SetFill(bool value) { Name.FillMenuWidth = value; Combo.FillMenuWidth = value; Hidden.FillMenuWidth = value; }
        public void Dispose() => Ribbon.Dispose();
    }
    private sealed class Record
    {
        internal Dictionary<RibbonItem, Rectangle> Fields { get; } = [];
        internal Dictionary<string, Rectangle> Text { get; } = [];
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawTextBox(c, item, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field) { record.Fields[item] = field; base.DrawComboBox(c, item, field); }
    }
}
