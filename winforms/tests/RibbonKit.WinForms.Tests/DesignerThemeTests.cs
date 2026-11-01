using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerThemeTests
{
    [Fact]
    public void Defaults_reset_and_direct_assignments_preserve_application_theme_precedence() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!;
        Assert.Null(ribbon.BuiltInTheme); Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme); Assert.False(property.ShouldSerializeValue(ribbon));
        var custom = RibbonThemes.Office2024Light with { Accent = Color.DarkMagenta, Background = Color.Beige };
        ribbon.Theme = custom; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme); Assert.True(property.ShouldSerializeValue(ribbon));
        property.ResetValue(ribbon); Assert.Null(ribbon.BuiltInTheme); Assert.Same(custom, ribbon.Theme); Assert.False(property.ShouldSerializeValue(ribbon));
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        // Even assignment of the current effective value establishes application ownership.
        ribbon.Theme = ribbon.Theme; Assert.Null(ribbon.BuiltInTheme); Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.BuiltInTheme = (RibbonBuiltInTheme)12);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = custom with { BodyHeight = -1 });
        Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Explicit_selection_serializes_as_an_enum_and_default_or_reset_omits_assignment(bool customApplicationTheme) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        if (customApplicationTheme) ribbon.Theme = ribbon.Theme with { Accent = Color.DarkMagenta };
        Assert.Empty(Serialize(ribbon).OfType<CodeAssignStatement>());
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        var assignment = Assert.Single(Serialize(ribbon).OfType<CodeAssignStatement>());
        Assert.Equal(nameof(ribbon.BuiltInTheme), Assert.IsType<CodePropertyReferenceExpression>(assignment.Left).PropertyName);
        Assert.Equal(nameof(RibbonBuiltInTheme.Office2024Light), Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName);
        ribbon.BuiltInTheme = null; Assert.Empty(Serialize(ribbon).OfType<CodeAssignStatement>());
        Assert.Equal(customApplicationTheme ? Color.DarkMagenta : RibbonThemes.Office2024Light.Accent, ribbon.Theme.Accent);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Root_choice_is_detached_and_cancel_or_unchanged_accept_preserves_the_custom_value(bool acceptUnchanged) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var custom = ribbon.Theme with { Accent = Color.DarkMagenta }; ribbon.Theme = custom;
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var tree = (TreeView)dialog.Controls.Find("StructureTree", true).Single(); _ = tree.Handle;
        var choice = (ComboBox)dialog.Controls.Find("BuiltInTheme", true).Single();
        Assert.Equal(2, choice.Items.Count); Assert.Equal(0, choice.SelectedIndex); Assert.Equal("Custom application theme", draft.ApplicationTheme);
        choice.SelectedIndex = 1; Assert.Equal("Office2024Light", draft.BuiltInTheme);
        Assert.Null(ribbon.BuiltInTheme); Assert.Same(custom, ribbon.Theme); Assert.Equal(0, host.Transactions);
        if (acceptUnchanged) { choice.SelectedIndex = 0; dialog.AcceptDraft(); }
        else dialog.DialogResult = DialogResult.Cancel;
        Assert.Same(custom, ribbon.Theme); Assert.Null(ribbon.BuiltInTheme); Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Theme_and_command_changes_commit_once_and_preserve_authored_owners_and_startup_state() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var selected = ribbon.SelectedTab; var group = ribbon.Tabs[0].Groups[0]; var command = group.Items[0]; var file = ribbon.FileMenu;
        var contextual = new RibbonTab { Text = "Context", Visible = false, IsContextual = true, ContextualColor = Color.DarkCyan }; ribbon.Tabs.Add(contextual);
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = "Office2024Light";
        draft.Nodes.Single(node => node.Kind == "Button").Properties.Single(property => property.Name == "Text").Value = "Updated";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, host.Committed);
        Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme); Assert.Same(command, group.Items[0]); Assert.Equal("Updated", command.Text);
        Assert.Same(selected, ribbon.SelectedTab); Assert.Same(file, ribbon.FileMenu); Assert.False(file.Visible);
        Assert.False(contextual.Visible); Assert.Equal(Color.DarkCyan, contextual.ContextualColor);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, RibbonDesignCommitter.Read(ribbon, host))); Assert.Equal(1, host.Transactions);
    });

    [Theory]
    [InlineData("unknown")] [InlineData("custom-change")] [InlineData("selection-change")]
    public void Invalid_or_stale_theme_drafts_do_not_start_a_transaction(string kind) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        if (kind == "unknown") draft.BuiltInTheme = "Dark";
        if (kind == "custom-change") ribbon.Theme = ribbon.Theme with { Accent = Color.DarkMagenta };
        if (kind == "selection-change") ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Framework_undo_redo_restores_the_custom_theme_and_noop_accept_creates_no_unit() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        var custom = ribbon.Theme with { Background = Color.Beige }; ribbon.Theme = custom;
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new UndoRecorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = "Office2024Light";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, undo.Count);
        undo.Toggle(0); Assert.Null(ribbon.BuiltInTheme); Assert.Same(custom, ribbon.Theme);
        undo.Toggle(0); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme); Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, RibbonDesignCommitter.Read(ribbon, host))); Assert.Equal(1, undo.Count);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = ""; RibbonDesignCommitter.Apply(ribbon, host, draft);
        Assert.Same(custom, ribbon.Theme); Assert.Equal(2, undo.Count);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Both_previews_use_the_draft_palette_without_commands_or_native_page_paint(bool builtIn) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var body = new PaintCounter(); var page = new RibbonBackstagePageItem { Text = "Info", Content = body };
        ribbon.Backstage.Items.Add(page); ribbon.Backstage.FooterItems.Add(new RibbonMenuSeparatorItem());
        ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Exit" });
        ribbon.Theme = ribbon.Theme with { Background = Color.Beige, BackstageNavigationBackground = Color.Pink };
        var renderer = new PaletteRecorder(); ribbon.Renderer = renderer;
        var expected = builtIn ? RibbonThemes.Office2024Light : ribbon.Theme;
        int clicks = 0, selections = 0; ((RibbonButtonItem)ribbon.Tabs[0].Groups[0].Items[0]).Click += (_, _) => clicks++;
        ribbon.SelectedTabChanged += (_, _) => selections++;
        var draft = RibbonDesignCommitter.Read(ribbon, host); var before = draft.Revision;
        draft.BuiltInTheme = builtIn ? "Office2024Light" : "";
        var result = RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_THEME_CAPTURE") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, builtIn ? "built-in-ribbon.png" : "application-ribbon.png"), Convert.FromBase64String(result.RibbonPng));
            File.WriteAllBytes(Path.Combine(directory, builtIn ? "built-in-backstage.png" : "application-backstage.png"), Convert.FromBase64String(result.BackstagePng));
        }
        Assert.Same(expected, renderer.RibbonTheme); Assert.Same(expected, renderer.BackstageTheme);
        foreach (string png in new[] { result.RibbonPng, result.BackstagePng })
        { using var stream = new MemoryStream(Convert.FromBase64String(png)); using var image = Image.FromStream(stream); Assert.True(image.Width >= 640); }
        Assert.Equal(before, RibbonDesignCommitter.Read(ribbon, host).Revision); Assert.Null(ribbon.BuiltInTheme);
        Assert.Null(body.Parent); Assert.Equal(0, body.Paints); Assert.Equal(0, clicks); Assert.Equal(0, selections);
        Assert.Equal(0, host.Transactions); Assert.False(ribbon.IsBackstageOpen); Assert.False(ribbon.FileMenu.Visible);
    });

    [Theory]
    [InlineData(1f)] [InlineData(1.25f)] [InlineData(1.5f)] [InlineData(2f)]
    public void Root_theme_actions_fit_at_small_and_large_synthetic_scales(float scale) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var dialog = new RibbonEditorDialog(RibbonDesignCommitter.Read(ribbon, host), _ => null, draft => RibbonThemePreviewRenderer.Render(ribbon, host, draft));
        _ = dialog.Handle; dialog.Scale(new SizeF(scale, scale));
        foreach (var logical in new[] { new Size(780, 520), new Size(1100, 760) })
        {
            dialog.ClientSize = new((int)(logical.Width * scale), (int)(logical.Height * scale)); Layout(dialog);
            var group = dialog.Controls.Find("RibbonTheme", true).Single();
            var choice = dialog.Controls.Find("BuiltInTheme", true).Single(); var button = dialog.Controls.Find("PreviewTheme", true).Single();
            var animation = dialog.Controls.Find("AnimationLevel", true).Single();
            Assert.True(group.Height > 60 * scale); Assert.True(button.Enabled);
            Assert.True(choice.Width >= 250 * scale); Assert.True(button.Bottom <= button.Parent!.ClientSize.Height);
            Assert.True(button.Right <= button.Parent.ClientSize.Width); Assert.True(button.Top >= choice.Bottom);
            Assert.True(animation.Top >= choice.Bottom); Assert.True(animation.Bottom <= button.Top); Assert.True(animation.Width >= 250 * scale);
        }
        if (scale == 1 && Environment.GetEnvironmentVariable("RIBBONKIT_THEME_CAPTURE") is { Length: > 0 } directory)
        {
            dialog.Show(); Application.DoEvents();
            Directory.CreateDirectory(directory); using var pixels = new Bitmap(dialog.Width, dialog.Height);
            dialog.DrawToBitmap(pixels, new(Point.Empty, pixels.Size)); pixels.Save(Path.Combine(directory, "ribbon-root-editor.png"));
            dialog.Hide();
        }
    });

    [Fact]
    public void Preview_window_contains_both_inert_scrollable_presentations() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        using var editor = new RibbonEditorDialog(RibbonDesignCommitter.Read(ribbon, host), _ => null, draft => RibbonThemePreviewRenderer.Render(ribbon, host, draft));
        using var preview = editor.CreateThemePreview();
        var tabs = Assert.IsType<TabControl>(preview.Controls.Find("ThemePreviewTabs", true).Single());
        Assert.Equal(new[] { "Ribbon", "Backstage" }, tabs.TabPages.Cast<TabPage>().Select(page => page.Text));
        Assert.All(tabs.TabPages.Cast<TabPage>(), page => { Assert.True(page.AutoScroll); Assert.IsType<PictureBox>(Assert.Single(page.Controls.Cast<Control>())); });
        if (Environment.GetEnvironmentVariable("RIBBONKIT_THEME_CAPTURE") is { Length: > 0 } directory)
        {
            preview.Show(); Application.DoEvents();
            for (int index = 0; index < tabs.TabPages.Count; index++)
            {
                tabs.SelectedIndex = index; Application.DoEvents();
                using var pixels = new Bitmap(preview.Width, preview.Height); preview.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
                pixels.Save(Path.Combine(directory, "preview-window-" + index + ".png"));
            }
        }
        Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Theme_preview_never_initializes_declared_hidden_or_disabled_native_pages() => FoundationTests.Sta(() =>
    {
        NeverCreateContent.InstancesCreated = 0;
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.Site = new DesignSite(ribbon.Site!);
        ribbon.BeginInit();
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Hidden", Visible = false, ContentType = typeof(NeverCreateContent) });
        ribbon.Backstage.FooterItems.Add(new RibbonBackstagePageItem { Text = "Disabled", Enabled = false, ContentType = typeof(NeverCreateContent) });
        ribbon.EndInit();
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.BuiltInTheme = "Office2024Light";
        RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Equal(0, NeverCreateContent.InstancesCreated); Assert.All(ribbon.Backstage.NavigationItems.OfType<RibbonBackstagePageItem>(), page => Assert.Null(page.Content));
        Assert.Equal(0, host.Transactions);
    });

    private static void Layout(Control control)
    { control.PerformLayout(); foreach (Control child in control.Controls) Layout(child); control.PerformLayout(); }

    private static CodeStatement[] Serialize(RibbonControl ribbon)
    {
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!);
        Assert.Empty(serialization.Errors.Cast<object>()); return statements.Cast<CodeStatement>().ToArray();
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle(int index) => _units[index].Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
    private sealed class PaintCounter : UserControl
    {
        internal int Paints;
        protected override void OnPaint(PaintEventArgs e) { Paints++; base.OnPaint(e); }
    }
    public sealed class NeverCreateContent : UserControl
    {
        internal static int InstancesCreated;
        public NeverCreateContent() => InstancesCreated++;
    }
    private sealed class DesignSite(ISite original) : ISite
    {
        public IComponent Component => original.Component; public IContainer? Container => original.Container;
        public bool DesignMode => true; public string? Name { get => original.Name; set => original.Name = value; }
        public object? GetService(Type serviceType) => original.GetService(serviceType);
    }
    private sealed class PaletteRecorder : RibbonOffice2024Renderer
    {
        internal RibbonTheme? RibbonTheme, BackstageTheme;
        public override void DrawSurface(RibbonPaintContext context, bool popup) { RibbonTheme = context.Theme; base.DrawSurface(context, popup); }
        public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth) { BackstageTheme = context.Theme; base.DrawBackstageBackground(context, navigationWidth); }
    }
}
