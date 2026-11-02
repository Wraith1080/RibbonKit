using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerBackdropTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly DesignSurface Surface;
        internal readonly IDesignerHost Host;
        internal readonly Form Form;
        internal readonly RibbonControl Ribbon;
        internal Fixture(bool ribbonForm = true)
        {
            Surface = new DesignSurface(ribbonForm ? typeof(RibbonForm) : typeof(Form));
            Host = (IDesignerHost)Surface.GetService(typeof(IDesignerHost))!;
            Form = (Form)Host.RootComponent;
            Form.Site!.Name = "designerForm"; Form.Name = "designerForm";
            Ribbon = (RibbonControl)Host.CreateComponent(typeof(RibbonControl), "ribbon");
            Form.Controls.Add(Ribbon);
            if (Form is RibbonForm hostForm) hostForm.Ribbon = Ribbon;
            var home = (RibbonTab)Host.CreateComponent(typeof(RibbonTab), "homeTab"); home.Text = "Home"; Ribbon.Tabs.Add(home);
        }
        internal RibbonDesignSnapshot Read() => RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(Ribbon, Host).Serialize());
        public void Dispose() => Surface.Dispose();
    }

    [Theory]
    [InlineData("None")] [InlineData("Mica")] [InlineData("Acrylic")]
    public void Window_backdrop_dropdown_edits_a_detached_draft_and_commits_the_form_property(string choice) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var form = (RibbonForm)f.Form; form.Backdrop = RibbonWindowBackdrop.Mica;
        var selected = f.Ribbon.SelectedTab; var draft = f.Read();
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(f.Ribbon, f.Host, value); return null; });
        var combo = (ComboBox)dialog.Controls.Find("Backdrop", true).Single();
        Assert.True(combo.Enabled); Assert.Equal(ComboBoxStyle.DropDownList, combo.DropDownStyle);
        Assert.Equal(new[] { "None", "Mica", "Acrylic" }, combo.Items.Cast<string>()); Assert.Equal("Mica", combo.SelectedItem);
        combo.SelectedItem = choice; Assert.Equal(choice, draft.Backdrop); Assert.Equal(RibbonWindowBackdrop.Mica, form.Backdrop);
        RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft); Assert.Equal(RibbonWindowBackdrop.Mica, form.Backdrop);
        if (choice == "Acrylic" && Environment.GetEnvironmentVariable("RIBBONKIT_THEME_CAPTURE") is { Length: > 0 } directory)
        {
            dialog.Show(); Application.DoEvents(); Directory.CreateDirectory(directory);
            using var pixels = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
            pixels.Save(Path.Combine(directory, "ribbon-backdrop-settings.png")); dialog.Hide();
        }
        dialog.AcceptDraft(); Assert.Equal(Enum.Parse<RibbonWindowBackdrop>(choice), form.Backdrop);
        Assert.Same(selected, f.Ribbon.SelectedTab); Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Cancel_and_unchanged_ok_leave_form_settings_and_undo_history_untouched(bool acceptUnchanged) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var undo = Recorder(f);
        var draft = f.Read(); using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(f.Ribbon, f.Host, value); return null; });
        var combo = (ComboBox)dialog.Controls.Find("Backdrop", true).Single(); combo.SelectedItem = "Acrylic";
        if (acceptUnchanged) { combo.SelectedItem = "None"; dialog.AcceptDraft(); } else dialog.DialogResult = DialogResult.Cancel;
        Assert.Equal(RibbonWindowBackdrop.None, ((RibbonForm)f.Form).Backdrop); Assert.Equal(0, undo.Count);
    });

    [Theory]
    [InlineData("Blur")] [InlineData("1")] [InlineData("mica")]
    public void Invalid_backdrop_is_rejected_before_any_live_change(string value) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var undo = Recorder(f);
        var draft = f.Read(); draft.Backdrop = value; draft.AnimationLevel = "Expressive";
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft));
        Assert.Equal(RibbonWindowBackdrop.None, ((RibbonForm)f.Form).Backdrop); Assert.Equal(RibbonAnimationLevel.None, f.Ribbon.AnimationLevel); Assert.Equal(0, undo.Count);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void External_backdrop_or_host_changes_invalidate_an_open_draft(bool detach) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var draft = f.Read();
        if (detach) f.Form.Controls.Remove(f.Ribbon); else ((RibbonForm)f.Form).Backdrop = RibbonWindowBackdrop.Mica;
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft));
    });

    [Fact]
    public void Ordinary_forms_disable_the_choice_and_cannot_be_spoofed_by_draft_metadata() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(false); var draft = f.Read(); Assert.False(draft.CanSetBackdrop);
        using var dialog = new RibbonEditorDialog(draft, _ => null);
        Assert.False(((ComboBox)dialog.Controls.Find("Backdrop", true).Single()).Enabled);
        Assert.False(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        draft.CanSetBackdrop = true; draft.Backdrop = "Acrylic";
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft));
    });

    [Fact]
    public void Form_backdrop_and_ribbon_edits_share_one_framework_undo_redo_unit() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture();
        var entry = (RibbonButtonItem)f.Host.CreateComponent(typeof(RibbonButtonItem), "saveCommand"); f.Ribbon.Backstage.Items.Add(entry);
        var backstage = f.Ribbon.Backstage; using var undo = Recorder(f); var draft = f.Read();
        draft.Backdrop = "Acrylic"; draft.BuiltInTheme = "Office2024Light"; draft.AnimationLevel = "Expressive"; draft.TabContentAnimation = "SlideDown";
        draft.NavigationBackgroundOpacity = 0;
        draft.Tabs[0].Properties.Single(property => property.Name == "Text").Value = "Edited Home";
        Assert.True(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft)); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal(RibbonWindowBackdrop.None, ((RibbonForm)f.Form).Backdrop); Assert.Null(f.Ribbon.BuiltInTheme);
        Assert.Equal(1, backstage.NavigationBackgroundOpacity); Assert.Same(backstage, f.Ribbon.Backstage); Assert.Same(entry, Assert.Single(backstage.Items));
        Assert.Equal(RibbonAnimationLevel.None, f.Ribbon.AnimationLevel); Assert.Equal(RibbonTabContentAnimation.Fade, f.Ribbon.TabContentAnimation); Assert.Equal("Home", f.Ribbon.Tabs[0].Text);
        undo.Toggle(); Assert.Equal(RibbonWindowBackdrop.Acrylic, ((RibbonForm)f.Form).Backdrop); Assert.Equal(RibbonBuiltInTheme.Office2024Light, f.Ribbon.BuiltInTheme);
        Assert.Equal(0, backstage.NavigationBackgroundOpacity); Assert.Same(backstage, f.Ribbon.Backstage); Assert.Same(entry, Assert.Single(backstage.Items));
        Assert.Equal(RibbonAnimationLevel.Expressive, f.Ribbon.AnimationLevel); Assert.Equal(RibbonTabContentAnimation.SlideDown, f.Ribbon.TabContentAnimation); Assert.Equal("Edited Home", f.Ribbon.Tabs[0].Text);
    });

    [Theory]
    [InlineData(1f, true, 0f)] [InlineData(0f, false, 1f)] [InlineData(.42f, true, .42f)]
    public void Transparent_backstage_checkbox_edits_only_the_detached_navigation_preference(float initial, bool check, float expected) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Backstage.NavigationBackgroundOpacity = initial;
        var draft = f.Read(); using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(f.Ribbon, f.Host, value); return null; });
        var checkbox = (CheckBox)dialog.Controls.Find("TransparentBackstage", true).Single();
        Assert.Equal(initial < 1, checkbox.Checked); checkbox.Checked = check;
        Assert.Equal(expected, draft.NavigationBackgroundOpacity); Assert.Equal(initial, f.Ribbon.Backstage.NavigationBackgroundOpacity);
        RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft); Assert.Equal(initial, f.Ribbon.Backstage.NavigationBackgroundOpacity);
        if (initial == 1 && Environment.GetEnvironmentVariable("RIBBONKIT_THEME_CAPTURE") is { Length: > 0 } directory)
        {
            dialog.Show(); Application.DoEvents(); Directory.CreateDirectory(directory);
            using var pixels = new Bitmap(dialog.Width, dialog.Height); dialog.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
            pixels.Save(Path.Combine(directory, "ribbon-transparent-backstage-settings.png")); dialog.Hide();
        }
        dialog.AcceptDraft(); Assert.Equal(expected, f.Ribbon.Backstage.NavigationBackgroundOpacity);
        Assert.Equal(RibbonWindowBackdrop.None, ((RibbonForm)f.Form).Backdrop); Assert.Equal(RibbonFileMenuMode.CommandMenu, f.Ribbon.FileMenuMode);
        Assert.False(f.Ribbon.IsBackstageOpen);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Transparent_backstage_cancel_and_unchanged_ok_preserve_partial_opacity_and_undo(bool acceptUnchanged) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.Backstage.NavigationBackgroundOpacity = .42f; using var undo = Recorder(f);
        var draft = f.Read(); using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(f.Ribbon, f.Host, value); return null; });
        if (acceptUnchanged) dialog.AcceptDraft();
        else { ((CheckBox)dialog.Controls.Find("TransparentBackstage", true).Single()).Checked = false; dialog.DialogResult = DialogResult.Cancel; }
        Assert.Equal(.42f, f.Ribbon.Backstage.NavigationBackgroundOpacity); Assert.Equal(0, undo.Count);
    });

    [Theory]
    [InlineData(-.1f)] [InlineData(1.1f)] [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)]
    public void Transparent_backstage_rejects_invalid_opacity_before_mutation(float value) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var undo = Recorder(f); var draft = f.Read(); draft.NavigationBackgroundOpacity = value; draft.Backdrop = "Mica";
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, draft));
        Assert.Equal(1, f.Ribbon.Backstage.NavigationBackgroundOpacity); Assert.Equal(RibbonWindowBackdrop.None, ((RibbonForm)f.Form).Backdrop); Assert.Equal(0, undo.Count);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Transparent_backstage_edits_invalidate_the_other_open_editor(bool ribbonFirst) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var root = f.Read(); root.NavigationBackgroundOpacity = 0;
        var backstage = RibbonBackstageCommitter.Read(f.Ribbon, f.Host); backstage.Settings.Single(setting => setting.Name == "NavigationBackgroundOpacity").Value = "0.25";
        if (ribbonFirst)
        {
            Assert.True(RibbonDesignCommitter.Apply(f.Ribbon, f.Host, root));
            Assert.Throws<ArgumentException>(() => RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, backstage));
        }
        else
        {
            Assert.True(RibbonBackstageCommitter.Apply(f.Ribbon, f.Host, backstage));
            Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(f.Ribbon, f.Host, root));
            Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(f.Ribbon, f.Host, root));
        }
    });

    [Theory]
    [InlineData(1f)] [InlineData(0f)] [InlineData(.42f)]
    public void Transparent_backstage_serializes_the_existing_nested_property_and_resets_to_opaque(float value) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var ribbon = f.Ribbon; _ = f.Read(); ribbon.Backstage.NavigationBackgroundOpacity = value;
        var serialization = new DesignerSerializationManager(f.Host); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var assignments = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Backstage)]!)
            .OfType<CodeAssignStatement>().Where(statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "NavigationBackgroundOpacity" }).ToArray();
        Assert.Empty(serialization.Errors.Cast<object>());
        if (value == 1) Assert.Empty(assignments);
        else
        {
            var assignment = Assert.Single(assignments); Assert.Equal(value, Assert.IsType<CodePrimitiveExpression>(assignment.Right).Value);
            Assert.Equal("Backstage", Assert.IsType<CodePropertyReferenceExpression>(((CodePropertyReferenceExpression)assignment.Left).TargetObject).PropertyName);
        }
        var property = TypeDescriptor.GetProperties(ribbon.Backstage)[nameof(ribbon.Backstage.NavigationBackgroundOpacity)]!;
        property.ResetValue(ribbon.Backstage); Assert.Equal(1, ribbon.Backstage.NavigationBackgroundOpacity); Assert.False(property.ShouldSerializeValue(ribbon.Backstage));
    });

    [Fact]
    public void Transparent_backstage_nested_design_site_keeps_model_and_content_ownership() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var body = new Panel();
        var page = (RibbonBackstagePageItem)f.Host.CreateComponent(typeof(RibbonBackstagePageItem), "infoPage"); page.Content = body; f.Ribbon.Backstage.Items.Add(page);
        var backstage = f.Ribbon.Backstage; int components = f.Host.Container.Components.Count, disposals = 0; backstage.Disposed += (_, _) => disposals++;
        _ = f.Read(); var site = Assert.IsAssignableFrom<INestedSite>(backstage.Site); Assert.Equal("ribbon.Backstage", site.FullName);
        Assert.Equal(components, f.Host.Container.Components.Count); Assert.Same(f.Ribbon, Assert.IsAssignableFrom<INestedContainer>(site.Container).Owner);
        _ = f.Read(); Assert.Same(site, backstage.Site); Assert.Same(backstage, f.Ribbon.Backstage); Assert.Same(page, Assert.Single(backstage.Items));
        f.Form.Dispose(); Assert.True(f.Ribbon.IsDisposed); Assert.True(backstage.IsDisposed); Assert.Equal(1, disposals);
        Assert.False(body.IsDisposed); Assert.Null(body.Parent);
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Existing_form_property_serializes_nondefaults_and_resets_to_none(RibbonWindowBackdrop value) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Backdrop = value };
        var property = TypeDescriptor.GetProperties(form)[nameof(form.Backdrop)]!;
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(form, "form");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonForm), typeof(CodeDomSerializer))!;
        var assignments = serializer.SerializeMember(manager, form, property).OfType<CodeAssignStatement>().ToArray();
        Assert.Empty(serialization.Errors.Cast<object>());
        if (value == RibbonWindowBackdrop.None) Assert.Empty(assignments);
        else Assert.Equal(value.ToString(), Assert.IsType<CodeFieldReferenceExpression>(Assert.Single(assignments).Right).FieldName);
        property.ResetValue(form); Assert.Equal(RibbonWindowBackdrop.None, form.Backdrop); Assert.False(property.ShouldSerializeValue(form));
    });

    private static UndoRecorder Recorder(Fixture fixture)
    {
        ((IServiceContainer)fixture.Surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(fixture.Host));
        return new UndoRecorder(fixture.Host);
    }
    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
