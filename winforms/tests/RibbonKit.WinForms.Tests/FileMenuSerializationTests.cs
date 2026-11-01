using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class FileMenuSerializationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Nested_file_content_serializes_with_instance_metadata_for_both_host_types(bool useRibbonForm) => FoundationTests.Sta(() =>
    {
        using Form form = useRibbonForm ? new RibbonForm() : new Form(); using var ribbon = new RibbonControl();
        form.Controls.Add(ribbon); if (form is RibbonForm host) host.Ribbon = ribbon;
        ribbon.FileMenu.Visible = true;
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = Assert.IsAssignableFrom<CodeDomSerializer>(manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer)));
        var member = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenu)]!;
        Assert.True(member.ShouldSerializeValue(ribbon));
        var statements = serializer.SerializeMember(manager, ribbon, member);
        Assert.True(serialization.Errors.Count == 0, string.Join("\n", serialization.Errors.Cast<object>().Select(error => error.ToString())));
        using var provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp"); using var generated = new StringWriter();
        foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, generated, new());
        var visibility = TypeDescriptor.GetProperties(ribbon.FileMenu)[nameof(ribbon.FileMenu.Visible)]!;
        Assert.True(statements.Cast<CodeStatement>().OfType<CodeAssignStatement>().Any(statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Visible" } && statement.Right is CodePrimitiveExpression { Value: true }),
            $"Count={statements.Count}; Default={((DefaultValueAttribute)visibility.Attributes[typeof(DefaultValueAttribute)]!).Value}; ShouldSerialize={visibility.ShouldSerializeValue(ribbon.FileMenu)}; Serializer={serializer.GetType().FullName}; Generated:\n{generated}");
    });

    [Fact]
    public void Default_runtime_and_browsable_filters_preserve_file_properties_and_explicit_design_only_filter_excludes_them() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.FileMenu.Visible = true;
        var runtime = TypeDescriptor.GetProperties(ribbon.FileMenu, [DesignOnlyAttribute.No]);
        var browsable = TypeDescriptor.GetProperties(ribbon.FileMenu, [BrowsableAttribute.Yes]);
        Assert.NotNull(runtime[nameof(ribbon.FileMenu.Visible)]); Assert.NotNull(runtime[nameof(ribbon.FileMenu.Text)]);
        Assert.NotNull(browsable[nameof(ribbon.FileMenu.Visible)]); Assert.NotNull(browsable[nameof(ribbon.FileMenu.Text)]);
        Assert.True(runtime[nameof(ribbon.FileMenu.Visible)]!.ShouldSerializeValue(ribbon.FileMenu));
        var designOnly = TypeDescriptor.GetProperties(ribbon.FileMenu, [DesignOnlyAttribute.Yes]);
        Assert.Null(designOnly[nameof(ribbon.FileMenu.Visible)]); Assert.Null(designOnly[nameof(ribbon.FileMenu.Text)]);
    });

    [Fact]
    public void Owned_file_defaults_reset_caption_keytip_and_visibility_to_their_actual_initial_values() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var file = ribbon.FileMenu; var properties = TypeDescriptor.GetProperties(file);
        Assert.Equal("File", file.Text); Assert.Equal("F", file.KeyTip); Assert.False(file.Visible);
        Assert.Equal("File", ((DefaultValueAttribute)properties[nameof(file.Text)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.Equal("F", ((DefaultValueAttribute)properties[nameof(file.KeyTip)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.Equal(false, ((DefaultValueAttribute)properties[nameof(file.Visible)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        file.Text = "Project"; file.KeyTip = "P"; file.Visible = true;
        properties[nameof(file.Text)]!.ResetValue(file); properties[nameof(file.KeyTip)]!.ResetValue(file); properties[nameof(file.Visible)]!.ResetValue(file);
        Assert.Equal("File", file.Text); Assert.Equal("F", file.KeyTip); Assert.False(file.Visible);
    });

    [Fact]
    public void Actual_code_dom_serializer_emits_enabled_file_visibility_and_keeps_default_caption_implicit() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.FileMenu.Visible = true;
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon.FileMenu, "fileMenu");
        var serializer = Assert.IsAssignableFrom<CodeDomSerializer>(manager.GetSerializer(typeof(RibbonDropDownItem), typeof(CodeDomSerializer)));
        var properties = TypeDescriptor.GetProperties(ribbon.FileMenu);
        Assert.True(properties[nameof(ribbon.FileMenu.Visible)]!.ShouldSerializeValue(ribbon.FileMenu));
        var visible = serializer.SerializeMember(manager, ribbon.FileMenu, properties[nameof(ribbon.FileMenu.Visible)]!);
        Assert.True(serialization.Errors.Count == 0, string.Join("\n", serialization.Errors.Cast<object>().Select(error => error.ToString())));
        Assert.Contains(visible.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Visible" } && statement.Right is CodePrimitiveExpression { Value: true });
        var caption = serializer.SerializeMember(manager, ribbon.FileMenu, properties[nameof(ribbon.FileMenu.Text)]!);
        Assert.DoesNotContain(caption.Cast<CodeStatement>().OfType<CodeAssignStatement>(), statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Text" });
    });

    [Fact]
    public void Ordinary_dropdowns_keep_their_generic_defaults_and_explicit_file_captions_can_still_be_authored() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); using var ordinary = new RibbonDropDownItem();
        var properties = TypeDescriptor.GetProperties(ordinary);
        Assert.Equal("", ((DefaultValueAttribute)properties[nameof(ordinary.Text)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.Equal(true, ((DefaultValueAttribute)properties[nameof(ordinary.Visible)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.True(ordinary.Visible); Assert.False(properties[nameof(ordinary.Visible)]!.ShouldSerializeValue(ordinary));
        var fileText = TypeDescriptor.GetProperties(ribbon.FileMenu)[nameof(ribbon.FileMenu.Text)]!;
        ribbon.FileMenu.Text = ""; Assert.True(fileText.ShouldSerializeValue(ribbon.FileMenu));
        ribbon.FileMenu.Text = "Project"; Assert.True(fileText.ShouldSerializeValue(ribbon.FileMenu));
        ribbon.FileMenu.Text = "File"; Assert.False(fileText.ShouldSerializeValue(ribbon.FileMenu));
    });
}
