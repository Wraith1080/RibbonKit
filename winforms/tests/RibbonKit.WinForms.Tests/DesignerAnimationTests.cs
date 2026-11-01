using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerAnimationTests
{
    [Theory]
    [InlineData("None")] [InlineData("Subtle")] [InlineData("Expressive")]
    public void Root_animation_dropdown_preserves_the_live_value_until_ok(string choice) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var selected = ribbon.SelectedTab; var command = ribbon.Tabs[0].Groups[0].Items[0];
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var combo = (ComboBox)dialog.Controls.Find("AnimationLevel", true).Single();
        Assert.Equal(ComboBoxStyle.DropDownList, combo.DropDownStyle);
        Assert.Equal(new[] { "None", "Subtle", "Expressive" }, combo.Items.Cast<string>()); Assert.Equal("Subtle", combo.SelectedItem);
        combo.SelectedItem = choice;
        Assert.Equal(choice, draft.AnimationLevel); Assert.Equal(RibbonAnimationLevel.Subtle, ribbon.AnimationLevel); Assert.Equal(0, host.Transactions);
        // The image preview stays inert even when the draft requests motion.
        RibbonThemePreviewRenderer.Render(ribbon, host, draft);
        Assert.Equal(RibbonAnimationLevel.Subtle, ribbon.AnimationLevel); Assert.Equal(0, host.Transactions);
        dialog.AcceptDraft(); Assert.Equal(Enum.Parse<RibbonAnimationLevel>(choice), ribbon.AnimationLevel);
        Assert.Equal(choice == "Subtle" ? 0 : 1, host.Committed);
        Assert.Same(selected, ribbon.SelectedTab); Assert.Same(command, ribbon.Tabs[0].Groups[0].Items[0]);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Cancel_and_unchanged_ok_do_not_apply_or_create_undo_entries(bool acceptUnchanged) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var combo = (ComboBox)dialog.Controls.Find("AnimationLevel", true).Single(); Assert.Equal("None", combo.SelectedItem);
        combo.SelectedItem = "Expressive";
        if (acceptUnchanged) { combo.SelectedItem = "None"; dialog.AcceptDraft(); }
        else dialog.DialogResult = DialogResult.Cancel;
        Assert.Equal(RibbonAnimationLevel.None, ribbon.AnimationLevel); Assert.Equal(0, host.Transactions);
    });

    [Theory]
    [InlineData("unknown")] [InlineData("numeric")] [InlineData("stale")]
    public void Invalid_or_externally_changed_animation_is_rejected_before_live_mutation(string kind) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        if (kind == "stale") ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        else draft.AnimationLevel = kind == "numeric" ? "1" : "Fast";
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(ribbon, host, draft));
        Assert.Equal(0, host.Transactions);
    });

    [Fact]
    public void Animation_and_theme_commit_in_one_framework_undo_unit() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(Form)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        ((IServiceContainer)surface.GetService(typeof(IServiceContainer))!).AddService(typeof(ComponentSerializationService), new CodeDomComponentSerializationService(host));
        using var undo = new UndoRecorder(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.AnimationLevel = "Expressive"; draft.BuiltInTheme = "Office2024Light"; draft.TabContentAnimation = "SlideDown";
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, undo.Count);
        undo.Toggle(); Assert.Equal(RibbonAnimationLevel.None, ribbon.AnimationLevel); Assert.Null(ribbon.BuiltInTheme); Assert.Equal(RibbonTabContentAnimation.Fade, ribbon.TabContentAnimation);
        undo.Toggle(); Assert.Equal(RibbonAnimationLevel.Expressive, ribbon.AnimationLevel); Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme); Assert.Equal(RibbonTabContentAnimation.SlideDown, ribbon.TabContentAnimation);
        Assert.False(RibbonDesignCommitter.Apply(ribbon, host, RibbonDesignCommitter.Read(ribbon, host))); Assert.Equal(1, undo.Count);
    });

    [Theory]
    [InlineData(RibbonAnimationLevel.None)] [InlineData(RibbonAnimationLevel.Subtle)] [InlineData(RibbonAnimationLevel.Expressive)]
    public void Existing_animation_property_serializes_nondefaults_and_reset_restores_none(RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { AnimationLevel = level };
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.AnimationLevel)]!;
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var assignments = serializer.SerializeMember(manager, ribbon, property).OfType<CodeAssignStatement>().ToArray();
        Assert.Empty(serialization.Errors.Cast<object>());
        if (level == RibbonAnimationLevel.None) Assert.Empty(assignments);
        else Assert.Equal(level.ToString(), Assert.IsType<CodeFieldReferenceExpression>(Assert.Single(assignments).Right).FieldName);
        property.ResetValue(ribbon); Assert.Equal(RibbonAnimationLevel.None, ribbon.AnimationLevel); Assert.False(property.ShouldSerializeValue(ribbon));
    });

    [Theory]
    [InlineData("Fade")] [InlineData("SlideDown")]
    public void Content_style_dropdown_is_detached_and_applies_the_shared_preference_once(string style) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignSnapshot.Deserialize(RibbonDesignCommitter.Read(ribbon, host).Serialize());
        using var dialog = new RibbonEditorDialog(draft, value => { RibbonDesignCommitter.Apply(ribbon, host, value); return null; });
        var combo = (ComboBox)dialog.Controls.Find("TabContentAnimation", true).Single();
        Assert.Equal(new[] { "Fade", "SlideDown" }, combo.Items.Cast<string>()); Assert.Equal("Fade", combo.SelectedItem);
        combo.SelectedItem = "SlideDown"; Assert.Equal("SlideDown", draft.TabContentAnimation);
        Assert.Equal(RibbonTabContentAnimation.Fade, ribbon.TabContentAnimation); Assert.Equal(0, host.Transactions);
        RibbonThemePreviewRenderer.Render(ribbon, host, draft); Assert.False(ribbon.IsAnimating);
        combo.SelectedItem = style; dialog.AcceptDraft();
        Assert.Equal(Enum.Parse<RibbonTabContentAnimation>(style), ribbon.TabContentAnimation);
        Assert.Equal(style == "Fade" ? 0 : 1, host.Committed);
    });

    [Fact]
    public void Cancelling_content_style_does_not_change_live_settings() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        using var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Cancel must not commit"));
        ((ComboBox)dialog.Controls.Find("TabContentAnimation", true).Single()).SelectedItem = "SlideDown";
        dialog.DialogResult = DialogResult.Cancel;
        Assert.Equal(RibbonTabContentAnimation.Fade, ribbon.TabContentAnimation); Assert.Equal(0, host.Transactions);
    });

    [Theory]
    [InlineData("unknown")] [InlineData("numeric")] [InlineData("stale")]
    public void Invalid_or_stale_content_styles_are_rejected_before_mutation(string kind) => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host);
        if (kind == "stale") ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;
        else draft.TabContentAnimation = kind == "numeric" ? "1" : "Slide";
        Assert.Throws<ArgumentException>(() => RibbonDesignCommitter.Apply(ribbon, host, draft));
        Assert.Throws<ArgumentException>(() => RibbonThemePreviewRenderer.Render(ribbon, host, draft));
        Assert.Equal(0, host.Transactions);
    });

    [Theory]
    [InlineData(RibbonTabContentAnimation.Fade)] [InlineData(RibbonTabContentAnimation.SlideDown)]
    public void Content_style_codegen_and_reset_preserve_the_existing_fade_default(RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { TabContentAnimation = style };
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.TabContentAnimation)]!;
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var assignments = serializer.SerializeMember(manager, ribbon, property).OfType<CodeAssignStatement>().ToArray();
        Assert.Empty(serialization.Errors.Cast<object>());
        if (style == RibbonTabContentAnimation.Fade) Assert.Empty(assignments);
        else Assert.Equal("SlideDown", Assert.IsType<CodeFieldReferenceExpression>(Assert.Single(assignments).Right).FieldName);
        property.ResetValue(ribbon); Assert.Equal(RibbonTabContentAnimation.Fade, ribbon.TabContentAnimation); Assert.False(property.ShouldSerializeValue(ribbon));
    });

    private sealed class UndoRecorder(IServiceProvider services) : UndoEngine(services)
    {
        private readonly List<UndoUnit> _units = [];
        internal int Count => _units.Count;
        internal void Toggle() => Assert.Single(_units).Undo();
        protected override void AddUndoUnit(UndoUnit unit) => _units.Add(unit);
    }
}
